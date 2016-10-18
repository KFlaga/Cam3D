//using CamCore;
//using MathNet.Numerics.LinearAlgebra;
//using MathNet.Numerics.LinearAlgebra.Double;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace CamControls
//{
//    class SGMDown
//    {
//        int DISP_RANGE = 10;
//        double PENALTY1 = 1;
//        double PENALTY2 = 2;

//        Vector<double> evaluate_path(Vector<double> prior,
//                   Vector<double> local,
//                   int path_intensity_gradient)
//        {
//            Vector<double> curr_cost = local;
//            for(int d = 0; d < DISP_RANGE; d++)
//            {
//                int e_smooth = int.MaxValue;
//                for(int d_p = 0; d_p < DISP_RANGE; d_p++)
//                {
//                    if(d_p - d == 0)
//                    {
//                        // No penality
//                        e_smooth = (int)Math.Min(e_smooth, prior[d_p]);
//                    }
//                    else if(Math.Abs(d_p - d) == 1)
//                    {
//                        // Small penality
//                        e_smooth = (int)Math.Min(e_smooth, prior[d_p] + PENALTY1);
//                    }
//                    else
//                    {
//                        // Large penality
//                        e_smooth = (int)Math.Min(e_smooth, prior[d_p] +
//                                   (int)Math.Max(PENALTY1, path_intensity_gradient != 0 ?
//                                   PENALTY2 / path_intensity_gradient : PENALTY2));
//                    }
//                }
//                curr_cost[d] += e_smooth;
//            }

//            // Normalize by subtracting min of prior cost
//            return curr_cost - prior;
//        }


//        void semi_global_matching_func(Matrix<double> left_image, Matrix<double> right_image)
//        {
//            IntVector2 size = new IntVector2(left_image.ColumnCount, left_image.RowCount);

//            // Processing all costs. W*H*D. D= DISP_RANGE
//            Matrix<double>[] costs = new DenseMatrix[DISP_RANGE];

//            int buffer_size = size.X * size.Y;
//            Vector<double> temporary;
//            //   std::fill(&temporary[0], &temporary[0] + DISP_RANGE, 255u);
//            //   std::fill(costs.data(), costs.data() + buffer_size,
//            //             temporary);

//            for(int j = 0; j < size.Y; j++)
//            {
//                for(int d = 0; d < DISP_RANGE; d++)
//                {
//                    for(int i = d; i < size.X; i++)
//                    {
//                        costs[d][i, j] = Math.Abs(left_image[i, j] - right_image[i - d, j]);
//                    }
//                }

//            }

//            Matrix<double> cost_image = new DenseMatrix(left_image.ColumnCount, 64);
//            for(int i = 0; i < left_image.ColumnCount; i++)
//            {
//                for(int j = 0; j < 64; j++)
//                {
//                    cost_image[i, j] = costs[j][i, 185];
//                }
//            }

//            Matrix<double> accumulated_costs = new DenseMatrix(left_image.ColumnCount, left_image.RowCount);
//            Matrix<double> dir_accumulated_costs = new DenseMatrix(left_image.ColumnCount, left_image.RowCount);

//            // Timer timer_total("\tCost Propagation");
//            {
//                //   Timer timer("\tCost Propagation [1,0]");
//                std::fill(dir_accumulated_costs.data(), dir_accumulated_costs.data() + buffer_size,
//                          AVector());
//                iterate_direction < 1,0 > (left_image, costs, dir_accumulated_costs );
//                write_image("effect_1_0.png", create_disparity_view(dir_accumulated_costs));
//                inplace_sum_views(accumulated_costs, dir_accumulated_costs);
//            }
//            {
//                //  Timer timer("\tCost Propagation [-1,0]");
//                std::fill(dir_accumulated_costs.data(), dir_accumulated_costs.data() + buffer_size,
//                          AVector());
//                iterate_direction < -1,0 > (left_image, costs, dir_accumulated_costs );
//                write_image("effect_-1_0.png", create_disparity_view(dir_accumulated_costs));
//                inplace_sum_views(accumulated_costs, dir_accumulated_costs);
//            }
//            {
//                //   Timer timer("\tCost Propagation [0,1]");
//                std::fill(dir_accumulated_costs.data(), dir_accumulated_costs.data() + buffer_size,
//                          AVector());
//                iterate_direction < 0,1 > (left_image, costs, dir_accumulated_costs );
//                write_image("effect_0_1.png", create_disparity_view(dir_accumulated_costs));
//                inplace_sum_views(accumulated_costs, dir_accumulated_costs);
//            }
//            {
//                //   Timer timer("\tCost Propagation [0,-1]");
//                std::fill(dir_accumulated_costs.data(), dir_accumulated_costs.data() + buffer_size,
//                          AVector());
//                iterate_direction < 0,-1 > (left_image, costs, dir_accumulated_costs );
//                write_image("effect_0_-1.png", create_disparity_view(dir_accumulated_costs));
//                inplace_sum_views(accumulated_costs, dir_accumulated_costs);
//            }
//            {
//                //    Timer timer("\tCost Propagation [1,1]");
//                std::fill(dir_accumulated_costs.data(), dir_accumulated_costs.data() + buffer_size,
//                          AVector());
//                iterate_direction < 1,1 > (left_image, costs, dir_accumulated_costs );
//                write_image("effect_1_1.png", create_disparity_view(dir_accumulated_costs));
//                inplace_sum_views(accumulated_costs, dir_accumulated_costs);
//            }
//            {
//                //   Timer timer("\tCost Propagation [-1,-1]");
//                std::fill(dir_accumulated_costs.data(), dir_accumulated_costs.data() + buffer_size,
//                          AVector());
//                iterate_direction < -1,-1 > (left_image, costs, dir_accumulated_costs );
//                write_image("effect_-1_-1.png", create_disparity_view(dir_accumulated_costs));
//                inplace_sum_views(accumulated_costs, dir_accumulated_costs);
//            }
//            {
//                //   Timer timer("\tCost Propagation [1,-1]");
//                std::fill(dir_accumulated_costs.data(), dir_accumulated_costs.data() + buffer_size,
//                          AVector());
//                iterate_direction < 1,-1 > (left_image, costs, dir_accumulated_costs );
//                write_image("effect_1_-1.png", create_disparity_view(dir_accumulated_costs));
//                inplace_sum_views(accumulated_costs, dir_accumulated_costs);
//            }
//            {
//                //    Timer timer("\tCost Propagation [-1,1]");
//                std::fill(dir_accumulated_costs.data(), dir_accumulated_costs.data() + buffer_size,
//                          AVector());
//                iterate_direction < -1,1 > (left_image, costs, dir_accumulated_costs );
//                write_image("effect_-1_1.png", create_disparity_view(dir_accumulated_costs));
//                inplace_sum_views(accumulated_costs, dir_accumulated_costs);
//            }

//        }

//        void iterate_direction(int DIRX, int DIRY, Matrix<double> left_image,
//                                 Matrix<double>[] costs,
//                                 Matrix<double> accumulated_costs)
//        {
//            int WIDTH = costs[0].ColumnCount;
//            int HEIGHT = costs[0].RowCount;

//            // Walk along the edges in a clockwise fashion
//            if(DIRX > 0)
//            {
//                // LEFT MOST EDGE
//                // Process every pixel along this edge
//                for(int j = 0; j < HEIGHT; j++)
//                {
//                    accumulated_costs[0, j] += costs[0, j];
//                }
//                for(int i = 1; i < WIDTH; i++)
//                {
//                    int jstart = Math.Max(0, 0 + DIRY * i);
//                    int jstop = Math.Min(HEIGHT, HEIGHT + DIRY * i);
//                    for(int j = jstart; j < jstop; j++)
//                    {
//                        accumulated_costs[i, j] =
//                          evaluate_path(accumulated_costs[i - DIRX, j - DIRY],
//                                         costs[i, j],
//                                         Math.Abs(left_image[i, j] - left_image[i - DIRX, j - DIRY]));
//                    }
//                }
//            }
//            if(DIRY > 0)
//            {
//                // TOP MOST EDGE
//                // Process every pixel along this edge only if DIRX ==
//                // 0. Otherwise skip the top left most pixel
//                for(int i = (DIRX <= 0 ? 0 : 1); i < WIDTH; i++)
//                {
//                    accumulated_costs[i, 0] += costs[i, 0];
//                }
//                for(int j = 1; j < HEIGHT; j++)
//                {
//                    int istart = Math.Max((DIRX <= 0 ? 0 : 1),
//                                 (DIRX <= 0 ? 0 : 1) + DIRX * j);
//                    int istop = Math.Min(WIDTH, WIDTH + DIRX * j);
//                    for(int i = istart; i < istop; i++)
//                    {
//                        accumulated_costs[i, j] =
//                          evaluate_path(accumulated_costs[i - DIRX, j - DIRY],
//                                         costs[i, j],
//                                         Math.Abs(left_image[i, j] - left_image[i - DIRX, j - DIRY]));
//                    }
//                }
//            }
//            if(DIRX < 0)
//            {
//                // RIGHT MOST EDGE
//                // Process every pixel along this edge only if DIRY ==
//                // 0. Otherwise skip the top right most pixel
//                for(int j = (DIRY <= 0 ? 0 : 1); j < HEIGHT; j++)
//                {
//                    accumulated_costs(WIDTH - 1, j) += costs(WIDTH - 1, j);
//                }
//                for(int i = WIDTH - 2; i >= 0; i--)
//                {
//                    int jstart = std::max((DIRY <= 0 ? 0 : 1),
//                                             (DIRY <= 0 ? 0 : 1) - DIRY * (i - WIDTH + 1));
//                    int jstop = std::min(HEIGHT, HEIGHT - DIRY * (i - WIDTH + 1));
//                    for(int j = jstart; j < jstop; j++)
//                    {
//                        accumulated_costs[i, j] =
//                          evaluate_path(accumulated_costs[i - DIRX, j - DIRY],
//                                         costs[i, j],
//                                         Math.Abs(left_image[i, j] - left_image[i - DIRX, j - DIRY]));
//                    }
//                }
//            }
//            if(DIRY < 0)
//            {
//                // BOTTOM MOST EDGE
//                // Process every pixel along this edge only if DIRX ==
//                // 0. Otherwise skip the bottom left and bottom right pixel
//                for(int i = (DIRX <= 0 ? 0 : 1); i < (DIRX >= 0 ? WIDTH : WIDTH - 1); i++)
//                {
//                    accumulated_costs(i, HEIGHT - 1) += costs(i, HEIGHT - 1);
//                }
//                for(int j = HEIGHT - 2; j >= 0; j--)
//                {
//                    int istart = Math.Max((DIRX <= 0 ? 0 : 1), (DIRX <= 0 ? 0 : 1) - DIRX * (j - HEIGHT + 1));
//                    int istop = Math.Min((DIRX >= 0 ? WIDTH : WIDTH - 1), (DIRX >= 0 ? WIDTH : WIDTH - 1) - DIRX * (j - HEIGHT + 1));
//                    for(int i = istart; i < istop; i++)
//                    {
//                        accumulated_costs[i, j] =
//                          evaluate_path(accumulated_costs[i - DIRX, j - DIRY],
//                                         costs[i, j],
//                                         Math.Abs(left_image[i, j] - left_image[i - DIRX, j - DIRY]));
//                    }
//                }
//            }
//        }
//    }
//}
